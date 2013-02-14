#include <stdio.h>
#include <stdlib.h>
#include <uhd/usrp/multi_usrp.hpp>

using namespace uhd;
using namespace uhd::usrp;

multi_usrp::sptr s_Radio;
rx_streamer::sptr s_RX;

extern "C" __declspec(dllexport) bool Init(double frequency, double rate)
{
	try {
		device_addr_t params("");
		s_Radio = multi_usrp::make(params);

		tune_request_t tuneRequest = tune_request_t(frequency);
		s_Radio->set_rx_freq(tuneRequest);
		s_Radio->set_rx_rate(rate);

		stream_args_t stream_args("fc32", "sc16");
		s_RX = s_Radio->get_rx_stream(stream_args);
		stream_cmd_t stream_cmd(stream_cmd_t::STREAM_MODE_START_CONTINUOUS);
		stream_cmd.stream_now = true;
		stream_cmd.time_spec = uhd::time_spec_t();
		s_Radio->issue_stream_cmd(stream_cmd);
		
		return true;
	} catch( uhd::key_error a ) {
		return false;
	} catch( uhd::assertion_error a ) {
		return false;
	}
}


extern "C" __declspec(dllexport) unsigned int GetSamples(float* samples, unsigned int maxSamples)
{
	uhd::rx_metadata_t md;
	unsigned int numSamples = s_RX->recv(samples, maxSamples, md, 1.0);
	if (md.error_code == uhd::rx_metadata_t::ERROR_CODE_NONE)
	{
		return numSamples;
	}
	return 0;
}
